# el-date-picker #

#### 可选择日期+时间 ####

	<el-date-picker v-model="xxx" type="datetime" value-format="yyyy-MM-dd HH:mm:ss" format="yyyy-MM-dd HH:mm:ss" placeholder="提示内容" />

*默认带“此刻”按钮*

#### 仅可选择日期 ####

	<el-date-picker v-model="xxx" type="date" :picker-options="maxToNowOption" value-format="yyyy-MM-dd" format="yyyy-MM-dd" placeholder="提示内容" />

*默认不带“此刻”按钮*

#### 绑定时间切换事件 ####

	<el-date-picker @change="changeDay" />

#### 让部分日期禁用不可选 ####

[参考文章](https://blog.csdn.net/gaolong123456/article/details/88800642)

例：不能超过当前时间

	<el-date-picker :picker-options="maxToNowOption" />

	maxToNowOption = {
	  disabledDate(time) {
	    return time > today
	  }
	}