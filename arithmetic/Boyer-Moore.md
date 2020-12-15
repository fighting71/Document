# Boyer-Moore-多数表决算法 #

[参考文章](https://gregable.com/2013/10/majority-vote-algorithm-find-majority.html)

[相关leetcode](https://leetcode.com/problems/majority-element-ii/)

### 问题说明 ###

假设您有一个未排序的值列表。

你想知道，在列表中存在一个值，该值出现的次数大于列表长度的1/2

出现此问题的一个常见原因可能是容错计算。您执行多个冗余计算，然后验证大多数结果是否一致。

### 简单的解决方案 ###

a.对列表进行排序，如果存在多数值，则它现在必须是中间值。并确定开始和结束下标

简单的解决方案是O(n lg n)由于排序。我们可以做得更好！

### Boyer-Moore 算法 ###

本文提出了一种Boyer-Moore算法：**Boyer-Moore多数表决算法**。该算法使用O(1)额外的空间和O(N)时间。它需要在输入列表上进行2次传递。它的实现也很简单，尽管要理解它的工作方式有些棘手。

在第一遍中我们生成单个候选值，如果存在多数，则为多数值。第二遍只计算该值的频率以确认。第一遍是有趣的部分。

在第一遍中，我们需要2个值：

	一个candidate值，最初设置为任何值。
	A count，最初设置为0。
	对于输入列表中的每个元素，我们首先检查该**count**值。如果计数**等于0**，则将设置**candidate**为当前元素的值。
	接下来，首先将元素的值与当前**candidate**值进行比较。
	如果它们相同，则**增加count1**。如果它们不同，则**减少count1**。

这样最终剩下的那个数就可能是出现频率最多的那个数

第二遍中

	遍历列表，获取**candidate**的出现次数

最终检查是否符合条件

（C#）参考代码：

	/// <summary>
    /// 获取出现频率超过1/2的数
    ///     若不存在则返回-1(假定arr中不存在-1)
    /// </summary>
    /// <param name="arr"></param>
    /// <returns></returns>
    public int GetNum(int[] arr)
    {
        // base check
        if (arr == null) return -1;

        int candidate = 0, count = 0;

        foreach (var item in arr)
        {
            if (item == candidate) count++;
            else if (count == 0)
            {
                candidate = item;
                count = 1;
            }
            else count--;
        }
        return arr.Where(u => u == candidate).Count() > arr.Length / 2 ? candidate : -1;
    }

**此算法对于1/n也有效**，

例如：计算(出现频率大于1/3的数)

定义两个candidate(初始不要重复即可)，count

在第一遍中，我们需要2个值：

	对于输入列表中的每个元素，我们首先检查(两个)**count**值。如果计数**等于0**，则将设置(count对应的)**candidate**为当前元素的值。
	接下来，首先将元素的值与(两个)**candidate**值进行比较。
	如果它们相同，则**增加(对应的)count1**。
	如果(都)不同，则**减少(两个)count1**。

同理可使用于任何出现频率大于1/n的场景，

定义(n-1)个candidate,count即可